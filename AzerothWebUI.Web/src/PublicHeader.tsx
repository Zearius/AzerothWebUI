import { NavLink } from 'react-router'
import ThemeToggle from './ThemeToggle'

function PublicHeader() {
  return (
    <header className="public-header">
      <nav className="public-nav">
        <NavLink to="/armory/characters">Armory</NavLink>
        <NavLink to="/login">Log In</NavLink>
        <NavLink to="/">Register</NavLink>
      </nav>
      <ThemeToggle />
    </header>
  )
}

export default PublicHeader
